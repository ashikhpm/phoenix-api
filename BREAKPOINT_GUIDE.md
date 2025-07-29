# Breakpoint Debugging Guide

## 🎯 How to Set Up and Use Breakpoints

### 1. **Visual Studio Code (Recommended)**

#### Setting Breakpoints
1. **Open the project in VS Code**
   ```bash
   code .
   ```

2. **Set breakpoints by clicking in the gutter**
   - Click in the left margin (gutter) next to line numbers
   - A red dot will appear indicating a breakpoint
   - **Recommended breakpoint locations:**
     - `UserController.cs` line 25 (GetAllUsers method start)
     - `UserController.cs` line 45 (GetUser method start)
     - `UserController.cs` line 65 (CreateUser method start)
     - `UserController.cs` line 95 (email validation)
     - `UserController.cs` line 110 (database save)

3. **Launch with debugging**
   ```bash
   # Method 1: Using VS Code
   # Press F5 or Ctrl+Shift+D to open debug panel
   # Select ".NET Core Launch (web)" configuration
   
   # Method 2: Using command line
   dotnet run --profile Debug
   ```

#### Debugging in VS Code
1. **Start debugging**: Press `F5`
2. **Continue**: Press `F5` or `Ctrl+F5`
3. **Step Over**: Press `F10`
4. **Step Into**: Press `F11`
5. **Step Out**: Press `Shift+F11`
6. **Stop**: Press `Shift+F5`

### 2. **Visual Studio**

#### Setting Breakpoints
1. **Open the project in Visual Studio**
2. **Set breakpoints**: Click in the gutter next to line numbers
3. **Launch with debugging**: Press `F5`

### 3. **Command Line Debugging**

#### Using Debugger
```bash
# Start with debugger attached
dotnet run --profile Debug

# In another terminal, attach debugger
dotnet run --configuration Debug --verbosity detailed
```

## 🔍 **Recommended Breakpoint Locations**

### **UserController.cs - Key Debug Points**

```csharp
// 1. Method Entry Points
[HttpGet]
public async Task<ActionResult<IEnumerable<User>>> GetAllUsers()
{
    // SET BREAKPOINT HERE - Line ~25
    try
    {
        _logger.LogInformation("Getting all users");
        var users = await _context.Users.ToListAsync();
        // SET BREAKPOINT HERE - Line ~30
        _logger.LogInformation("Retrieved {Count} users", users.Count);
        return Ok(users);
    }
    catch (Exception ex)
    {
        // SET BREAKPOINT HERE - Line ~35
        _logger.LogError(ex, "Error retrieving users");
        return StatusCode(500, "An error occurred while retrieving users");
    }
}

// 2. Database Operations
[HttpPost]
public async Task<ActionResult<User>> CreateUser([FromBody] User user)
{
    // SET BREAKPOINT HERE - Line ~65
    try
    {
        _logger.LogInformation("Creating new user: {Name}", user.Name);
        
        // SET BREAKPOINT HERE - Line ~70 (Validation)
        if (string.IsNullOrWhiteSpace(user.Name) || string.IsNullOrWhiteSpace(user.Email))
        {
            return BadRequest("Name and Email are required");
        }

        // SET BREAKPOINT HERE - Line ~75 (Email Check)
        var existingUser = await _context.Users
            .Where(u => u.Email.ToLower() == user.Email.ToLower())
            .FirstOrDefaultAsync();
            
        if (existingUser != null)
        {
            return BadRequest("Email already exists");
        }

        // SET BREAKPOINT HERE - Line ~85 (Database Save)
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        
        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
    }
    catch (Exception ex)
    {
        // SET BREAKPOINT HERE - Line ~95 (Error Handling)
        _logger.LogError(ex, "Error creating user: {Name}", user.Name);
        return StatusCode(500, "An error occurred while creating the user");
    }
}
```

### **UserDbContext.cs - Database Debug Points**

```csharp
public class UserDbContext : DbContext
{
    // SET BREAKPOINT HERE - Constructor
    public UserDbContext(DbContextOptions<UserDbContext> options) : base(options)
    {
    }

    // SET BREAKPOINT HERE - Model Configuration
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Debug model configuration here
    }
}
```

## 🚀 **Debugging Workflow**

### **Step 1: Set Breakpoints**
1. Open `Controllers/UserController.cs`
2. Set breakpoints at:
   - Line 25: `GetAllUsers()` method start
   - Line 65: `CreateUser()` method start
   - Line 75: Email validation logic
   - Line 85: Database save operation

### **Step 2: Start Debugging**
```bash
# Start the application in debug mode
dotnet run --profile Debug
```

### **Step 3: Trigger Breakpoints**
1. **Open Swagger UI**: `https://localhost:7001/swagger`
2. **Test endpoints**:
   - `GET /api/user` - Triggers GetAllUsers breakpoint
   - `POST /api/user` - Triggers CreateUser breakpoint
   - `GET /api/user/health` - Tests database connection

### **Step 4: Debug Information**
When breakpoint hits, you can inspect:
- **Variables**: `user`, `_context`, `_logger`
- **Call Stack**: See method call hierarchy
- **Watch Window**: Monitor specific variables
- **Immediate Window**: Execute expressions

## 🔧 **Debug Configuration**

### **launchSettings.json**
```json
{
  "profiles": {
    "Debug": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "swagger",
      "applicationUrl": "https://localhost:7001;http://localhost:5001",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

## 📊 **Debugging Tips**

### **1. Variable Inspection**
```csharp
// In debugger, you can inspect:
user.Name          // User's name
user.Email         // User's email
_context.Users     // DbSet of users
_context.Database  // Database context
```

### **2. Conditional Breakpoints**
- Right-click on breakpoint
- Set condition: `user.Email.Contains("@")`
- Breakpoint only hits when condition is true

### **3. Logging with Breakpoints**
```csharp
// Set breakpoint here to inspect logging
_logger.LogInformation("Creating new user: {Name}", user.Name);
```

### **4. Database Debugging**
```csharp
// Set breakpoint to inspect database operations
var users = await _context.Users.ToListAsync();
```

## 🐛 **Common Debug Scenarios**

### **Scenario 1: User Creation Fails**
1. Set breakpoint at line 65 (CreateUser start)
2. Set breakpoint at line 75 (email validation)
3. Set breakpoint at line 85 (database save)
4. Test with Swagger UI
5. Step through each breakpoint to identify issue

### **Scenario 2: Database Connection Issues**
1. Set breakpoint at line 140 (HealthCheck method)
2. Call `/api/user/health` endpoint
3. Inspect `canConnect` variable
4. Check connection string in debugger

### **Scenario 3: Validation Errors**
1. Set breakpoint at line 70 (validation logic)
2. Send invalid data via Swagger
3. Step through validation checks
4. Inspect `user` object properties

## 🎯 **Quick Debug Commands**

```bash
# Start debugging
dotnet run --profile Debug

# Build in debug mode
dotnet build --configuration Debug

# Run with detailed logging
dotnet run --verbosity detailed

# Test specific endpoint
curl -X GET "https://localhost:7001/api/user/health"
```

## 📝 **Debug Checklist**

- [ ] Set breakpoints in UserController.cs
- [ ] Start application in debug mode
- [ ] Open Swagger UI
- [ ] Test health check endpoint
- [ ] Test user creation endpoint
- [ ] Monitor console output
- [ ] Check database connection
- [ ] Verify error handling

## 🚨 **Troubleshooting Debug Issues**

### **Breakpoints Not Hitting**
1. Ensure application is running in debug mode
2. Check if breakpoints are set correctly
3. Verify endpoint is being called
4. Check console for errors

### **Debugger Not Attaching**
1. Use `dotnet run --profile Debug`
2. Check launchSettings.json configuration
3. Ensure no other instances are running
4. Restart debugging session

### **Variables Not Visible**
1. Ensure breakpoint is in correct scope
2. Check if variable is initialized
3. Use watch window for specific variables
4. Step through code to see variable values 