# Debug script for Phoenix Sangam API
Write-Host "=== Phoenix Sangam API Debug Script ===" -ForegroundColor Green

# Check if dotnet is available
Write-Host "1. Checking .NET installation..." -ForegroundColor Yellow
dotnet --version

# Clean and build the project
Write-Host "`n2. Cleaning and building project..." -ForegroundColor Yellow
dotnet clean
dotnet build

# Check if migrations exist
Write-Host "`n3. Checking migrations..." -ForegroundColor Yellow
if (Test-Path "Migrations") {
    Write-Host "Migrations folder exists" -ForegroundColor Green
    Get-ChildItem "Migrations" | ForEach-Object { Write-Host "  - $($_.Name)" -ForegroundColor Cyan }
} else {
    Write-Host "No migrations folder found" -ForegroundColor Red
}

# Try to update database
Write-Host "`n4. Attempting database update..." -ForegroundColor Yellow
try {
    dotnet ef database update
    Write-Host "Database update completed successfully" -ForegroundColor Green
} catch {
    Write-Host "Database update failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Start the application
Write-Host "`n5. Starting application..." -ForegroundColor Yellow
Write-Host "Application will be available at:" -ForegroundColor Cyan
Write-Host "  - HTTPS: https://localhost:7001" -ForegroundColor Cyan
Write-Host "  - HTTP:  http://localhost:5001" -ForegroundColor Cyan
Write-Host "  - Swagger: https://localhost:7001/swagger" -ForegroundColor Cyan
Write-Host "`nPress Ctrl+C to stop the application" -ForegroundColor Yellow

# Start the application
dotnet run --profile Debug 