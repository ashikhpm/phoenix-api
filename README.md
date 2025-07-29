# Phoenix Sangam API

A .NET 7 Web API with Entity Framework Core Code First approach and generic database support.

## Features

- Entity Framework Core Code First
- Generic database support (SQL Server, SQLite, PostgreSQL, MySQL)
- RESTful API with Swagger documentation
- User management with CRUD operations

## Database Configuration

The application supports multiple database providers. Configure your preferred database in `appsettings.json`:

### SQL Server (Default)
```json
{
  "Database": {
    "Provider": "SqlServer",
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=PhoenixSangamDb;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

### SQLite
```json
{
  "Database": {
    "Provider": "Sqlite",
    "SqliteConnection": "Data Source=PhoenixSangamDb.db"
  }
}
```

### PostgreSQL
```json
{
  "Database": {
    "Provider": "PostgreSql",
    "PostgreSqlConnection": "Host=localhost;Database=PhoenixSangamDb;Username=postgres;Password=password"
  }
}
```

### MySQL
```json
{
  "Database": {
    "Provider": "MySql",
    "MySqlConnection": "Server=localhost;Database=PhoenixSangamDb;User=root;Password=password;"
  }
}
```

## Getting Started

1. Update the database configuration in `appsettings.json`
2. Run Entity Framework migrations:
   ```bash
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```
3. Run the application:
   ```bash
   dotnet run
   ```
4. Access the API documentation at: `https://localhost:7001/swagger`

## API Endpoints

- `GET /api/user` - Get all users
- `GET /api/user/{id}` - Get user by ID
- `POST /api/user` - Create new user
- `PUT /api/user/{id}` - Update user
- `DELETE /api/user/{id}` - Delete user

## Project Structure

- `Controllers/` - API controllers
- `Data/` - Entity Framework DbContext
- `Models/` - Entity models
- `Configuration/` - Configuration classes
- `Extensions/` - Service extension methods 