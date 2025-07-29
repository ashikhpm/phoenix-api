using Microsoft.EntityFrameworkCore;
using phoenix_sangam_api.Data;
using phoenix_sangam_api.Configuration;
using System;

namespace phoenix_sangam_api.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddGenericDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var databaseConfig = new DatabaseConfig();
        configuration.GetSection("Database").Bind(databaseConfig);
        
        services.Configure<DatabaseConfig>(configuration.GetSection("Database"));
        
        // Enable legacy timestamp behavior for PostgreSQL
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        
        services.AddDbContext<UserDbContext>(options =>
        {
            var connectionString = databaseConfig.GetConnectionString();
            
            switch (databaseConfig.Provider?.ToLower())
            {
                case "sqlite":
                    options.UseSqlite(connectionString);
                    break;
                case "postgresql":
                    options.UseNpgsql(connectionString);
                    break;
                case "mysql":
                    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
                    break;
                case "sqlserver":
                default:
                    options.UseSqlServer(connectionString);
                    break;
            }
        });
        
        return services;
    }
} 