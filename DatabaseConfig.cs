namespace phoenix_sangam_api.Configuration;

public class DatabaseConfig
{
    public string Provider { get; set; } = "PostgreSql";
    public string DefaultConnection { get; set; } = string.Empty;
    public string SqliteConnection { get; set; } = string.Empty;
    public string PostgreSqlConnection { get; set; } = string.Empty;
    public string MySqlConnection { get; set; } = string.Empty;

    public string GetConnectionString()
    {
        return Provider?.ToLower() switch
        {
            "sqlite" => SqliteConnection,
            "postgresql" => PostgreSqlConnection,
            "mysql" => MySqlConnection,
            "sqlserver" or _ => DefaultConnection
        };
    }
} 