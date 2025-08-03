# CORS Configuration Guide

## Overview

The CORS (Cross-Origin Resource Sharing) configuration has been moved to the `appsettings.json` file for better configuration management and flexibility across different environments.

## Configuration Structure

### appsettings.json (Production)
```json
{
  "CorsSettings": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "http://localhost:3001",
      "https://localhost:3000",
      "https://localhost:3001"
    ],
    "AllowedMethods": [
      "GET",
      "POST",
      "PUT",
      "DELETE",
      "OPTIONS"
    ],
    "AllowedHeaders": [
      "*"
    ],
    "AllowCredentials": true
  }
}
```

### appsettings.Development.json (Development)
```json
{
  "CorsSettings": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "http://localhost:3001",
      "http://localhost:5173",
      "http://localhost:8080",
      "https://localhost:3000",
      "https://localhost:3001",
      "https://localhost:5173",
      "https://localhost:8080"
    ],
    "AllowedMethods": [
      "GET",
      "POST",
      "PUT",
      "DELETE",
      "OPTIONS",
      "PATCH"
    ],
    "AllowedHeaders": [
      "*"
    ],
    "AllowCredentials": true
  }
}
```

## Configuration Properties

### AllowedOrigins
- **Type**: Array of strings
- **Description**: List of allowed origin URLs
- **Example**: `["http://localhost:3000", "https://myapp.com"]`
- **Note**: Include both HTTP and HTTPS versions if needed

### AllowedMethods
- **Type**: Array of strings
- **Description**: List of allowed HTTP methods
- **Common Values**: `GET`, `POST`, `PUT`, `DELETE`, `OPTIONS`, `PATCH`
- **Note**: `OPTIONS` is required for preflight requests

### AllowedHeaders
- **Type**: Array of strings
- **Description**: List of allowed HTTP headers
- **Common Values**: `["*"]` (allows all headers), `["Content-Type", "Authorization"]`
- **Note**: Use `["*"]` for development, specify exact headers for production

### AllowCredentials
- **Type**: Boolean
- **Description**: Whether to allow credentials (cookies, authorization headers)
- **Default**: `true`
- **Note**: Required for JWT authentication

## Environment-Specific Configuration

### Development Environment
- **File**: `appsettings.Development.json`
- **Features**: 
  - More permissive origins (multiple localhost ports)
  - Additional HTTP methods (PATCH)
  - All headers allowed

### Production Environment
- **File**: `appsettings.json`
- **Features**:
  - Restricted origins (only necessary domains)
  - Standard HTTP methods
  - All headers allowed (can be restricted)

## Adding New Origins

### For Development
1. Edit `appsettings.Development.json`
2. Add your new origin to the `AllowedOrigins` array:
   ```json
   "AllowedOrigins": [
     "http://localhost:3000",
     "http://localhost:3001",
     "http://localhost:5173",
     "http://your-new-app:port"
   ]
   ```

### For Production
1. Edit `appsettings.json`
2. Add your production domain:
   ```json
   "AllowedOrigins": [
     "https://your-production-domain.com",
     "https://www.your-production-domain.com"
   ]
   ```

## Security Best Practices

### Development
- ✅ Allow multiple localhost origins for testing
- ✅ Allow all headers (`"*"`)
- ✅ Allow credentials for authentication testing

### Production
- ❌ Don't use `"*"` for origins
- ✅ Specify exact allowed origins
- ✅ Consider restricting headers to only necessary ones
- ✅ Use HTTPS origins only
- ✅ Regularly review and update allowed origins

## Troubleshooting

### Common Issues

1. **CORS Error: Origin not allowed**
   - Check if the origin is in `AllowedOrigins`
   - Ensure both HTTP and HTTPS versions are included if needed

2. **CORS Error: Method not allowed**
   - Verify the HTTP method is in `AllowedMethods`
   - Add `OPTIONS` to allow preflight requests

3. **CORS Error: Headers not allowed**
   - Check `AllowedHeaders` configuration
   - Use `["*"]` for development, specify exact headers for production

4. **Authentication not working**
   - Ensure `AllowCredentials` is set to `true`
   - Check that the origin is properly configured

### Testing CORS Configuration

1. **Check current configuration:**
   ```bash
   # The application will log CORS configuration on startup
   dotnet run
   ```

2. **Test from browser console:**
   ```javascript
   fetch('http://localhost:5276/api/users', {
     method: 'GET',
     credentials: 'include',
     headers: {
       'Authorization': 'Bearer your-token'
     }
   })
   .then(response => console.log(response))
   .catch(error => console.error('CORS Error:', error));
   ```

## Configuration Classes

### CorsSettings.cs
```csharp
public class CorsSettings
{
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
    public string[] AllowedMethods { get; set; } = Array.Empty<string>();
    public string[] AllowedHeaders { get; set; } = Array.Empty<string>();
    public bool AllowCredentials { get; set; } = true;
}
```

## Program.cs Integration

The CORS configuration is automatically loaded from `appsettings.json`:

```csharp
// Configure CORS from appsettings.json
var corsSettings = builder.Configuration.GetSection("CorsSettings").Get<CorsSettings>();
if (corsSettings == null)
{
    // Fallback to default settings if configuration is missing
    corsSettings = new CorsSettings
    {
        AllowedOrigins = new[] { "http://localhost:3000" },
        AllowedMethods = new[] { "GET", "POST", "PUT", "DELETE", "OPTIONS" },
        AllowedHeaders = new[] { "*" },
        AllowCredentials = true
    };
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowConfiguredOrigins", policy =>
    {
        policy.WithOrigins(corsSettings.AllowedOrigins)
              .WithHeaders(corsSettings.AllowedHeaders)
              .WithMethods(corsSettings.AllowedMethods);
        
        if (corsSettings.AllowCredentials)
        {
            policy.AllowCredentials();
        }
    });
});
```

## Migration from Hardcoded CORS

### Before (Hardcoded)
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocal3000",
        policy => policy.WithOrigins("http://localhost:3000")
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials());
});
```

### After (Configuration-based)
```csharp
// Configuration loaded from appsettings.json
var corsSettings = builder.Configuration.GetSection("CorsSettings").Get<CorsSettings>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowConfiguredOrigins", policy =>
    {
        policy.WithOrigins(corsSettings.AllowedOrigins)
              .WithHeaders(corsSettings.AllowedHeaders)
              .WithMethods(corsSettings.AllowedMethods);
        
        if (corsSettings.AllowCredentials)
        {
            policy.AllowCredentials();
        }
    });
});
```

## Benefits

1. **Environment Flexibility**: Different settings for development and production
2. **Easy Maintenance**: Change origins without recompiling
3. **Security**: Environment-specific security policies
4. **Scalability**: Easy to add new origins for different environments
5. **Configuration Management**: Centralized CORS configuration 